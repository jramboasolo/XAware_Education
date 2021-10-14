import bpy

bpy.context.scene.render.engine = 'CYCLES'
bpy.context.scene.cycles.bake_type = 'DIFFUSE'
bpy.context.scene.render.bake.use_pass_direct = False
bpy.context.scene.render.bake.use_pass_indirect = False
bpy.context.scene.render.bake.use_pass_color = True
obj = bpy.context.active_object
bpy.ops.object.modifier_add(type='DATA_TRANSFER')
obj.modifiers["DataTransfer"].data_types_loops = {'VCOL'} 
obj.modifiers["DataTransfer"].loop_mapping = 'TOPOLOGY'
for theta in range(-130,135,5):
    for phi in range(-40,45,5):
        #print('exposureClinician_'+str(theta)+'_'+str(phi))
        obj.modifiers["DataTransfer"].object = bpy.data.objects["exposureClinician_"+str(theta)+"_"+str(phi)]

        image_name = 'Texture_' + str(theta) + '_' + str(phi)
        img = bpy.data.images.new(image_name,1024,1024)

        mat = obj.data.materials[0]
        mat.use_nodes = True #Here it is assumed that the materials have been created with nodes, otherwise it would not be possible to assign a node for the Bake, so this step is a bit useless
        nodes = mat.node_tree.nodes
        texture_node =nodes.new('ShaderNodeTexImage')
        texture_node.name = 'Bake_node'
        texture_node.select = True
        nodes.active = texture_node
        texture_node.image = img #Assign the image to the node

        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.bake(type='DIFFUSE')



        img.filepath_raw = 'D:\\Ramboasolo\\texture_Save\\withShield_A\\'+ image_name + '.png'
        img.file_format = 'PNG'
        img.save()

        #img.save_render(filepath='D:\\Ramboasolo\\texture_Save\\'+ image_name+'.png')

        #bpy.ops.img.save_as(save_as_render=False, filepath="D:\\Ramboasolo\\texture_Save\\Texture_-5_-5_B2.png", relative_path=True, show_multiview=False, use_multiview=False)


        for n in mat.node_tree.nodes:
            if n.name == 'Bake_node':
                mat.node_tree.nodes.remove(n)
                
obj.modifiers.clear()